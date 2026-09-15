Feature: Collapsible sections
  As a visitor reading a long article
  I want expandable sections whose state is shareable
  So that I can focus on one part and link others straight to it

  Scenario: Opening a section reveals its content and updates the URL
	Given I open the Five Ways page
	When I open the first section
	Then the first section should be expanded
	And the URL should contain the first section

  Scenario: Opening another section collapses the first
	Given I open the Five Ways page
	When I open the first section
	And I open the second section
	Then only one section should be expanded

  Scenario: A shared section link opens the page with that section expanded
	Given I open the Five Ways page with the second section in the URL
	Then the second section should be expanded
